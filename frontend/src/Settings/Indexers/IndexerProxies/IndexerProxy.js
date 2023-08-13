import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditIndexerProxyModalConnector from './EditIndexerProxyModalConnector';
import styles from './IndexerProxy.css';

class IndexerProxy extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditIndexerProxyModalOpen: false,
      isDeleteIndexerProxyModalOpen: false
    };
  }

  //
  // Listeners

  onEditIndexerProxyPress = () => {
    this.setState({ isEditIndexerProxyModalOpen: true });
  };

  onEditIndexerProxyModalClose = () => {
    this.setState({ isEditIndexerProxyModalOpen: false });
  };

  onDeleteIndexerProxyPress = () => {
    this.setState({
      isEditIndexerProxyModalOpen: false,
      isDeleteIndexerProxyModalOpen: true
    });
  };

  onDeleteIndexerProxyModalClose= () => {
    this.setState({ isDeleteIndexerProxyModalOpen: false });
  };

  onConfirmDeleteIndexerProxy = () => {
    this.props.onConfirmDeleteIndexerProxy(this.props.id);
  };

  //
  // Render

  render() {
    const {
      id,
      name,
      tags,
      tagList,
      indexerList
    } = this.props;

    return (
      <Card
        className={styles.indexerProxy}
        overlayContent={true}
        onPress={this.onEditIndexerProxyPress}
      >
        <div className={styles.name}>
          {name}
        </div>

        <TagList
          tags={tags}
          tagList={tagList}
        />

        <div className={styles.indexers}>
          {
            tags.map((t) => {
              const indexers = _.filter(indexerList, { tags: [t] });

              if (!indexers || indexers.length === 0) {
                return null;
              }

              return indexers.map((i) => {
                return (
                  <Label
                    key={i.name}
                    kind={kinds.SUCCESS}
                  >
                    {i.name}
                  </Label>
                );
              });
            })
          }
        </div>

        {
          !tags || tags.length === 0 ?
            <Label
              kind={kinds.DISABLED}
              outline={true}
            >
              {translate('Disabled')}
            </Label> :
            null
        }

        <EditIndexerProxyModalConnector
          id={id}
          isOpen={this.state.isEditIndexerProxyModalOpen}
          onModalClose={this.onEditIndexerProxyModalClose}
          onDeleteIndexerProxyPress={this.onDeleteIndexerProxyPress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteIndexerProxyModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteIndexerProxy')}
          message={translate('DeleteIndexerProxyMessageText', { name })}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteIndexerProxy}
          onCancel={this.onDeleteIndexerProxyModalClose}
        />
      </Card>
    );
  }
}

IndexerProxy.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  tagList: PropTypes.arrayOf(PropTypes.object).isRequired,
  indexerList: PropTypes.arrayOf(PropTypes.object).isRequired,
  onConfirmDeleteIndexerProxy: PropTypes.func.isRequired
};

export default IndexerProxy;
