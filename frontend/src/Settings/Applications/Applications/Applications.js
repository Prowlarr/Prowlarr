import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddApplicationModal from './AddApplicationModal';
import Application from './Application';
import EditApplicationModalConnector from './EditApplicationModalConnector';
import styles from './Applications.css';

class Applications extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isAddApplicationModalOpen: false,
      isEditApplicationModalOpen: false
    };
  }

  //
  // Listeners

  onAddApplicationPress = () => {
    this.setState({ isAddApplicationModalOpen: true });
  };

  onAddApplicationModalClose = ({ applicationSelected = false } = {}) => {
    this.setState({
      isAddApplicationModalOpen: false,
      isEditApplicationModalOpen: applicationSelected
    });
  };

  onEditApplicationModalClose = () => {
    this.setState({ isEditApplicationModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      items,
      tagList,
      onConfirmDeleteApplication,
      ...otherProps
    } = this.props;

    const {
      isAddApplicationModalOpen,
      isEditApplicationModalOpen
    } = this.state;

    return (
      <FieldSet legend={translate('Applications')}>
        <PageSectionContent
          errorMessage={translate('ApplicationsLoadError')}
          {...otherProps}
        >
          <div className={styles.applications}>
            {
              items.map((item) => {
                return (
                  <Application
                    key={item.id}
                    {...item}
                    tagList={tagList}
                    onConfirmDeleteApplication={onConfirmDeleteApplication}
                  />
                );
              })
            }

            <Card
              className={styles.addApplication}
              onPress={this.onAddApplicationPress}
            >
              <div className={styles.center}>
                <Icon
                  name={icons.ADD}
                  size={45}
                />
              </div>
            </Card>
          </div>

          <AddApplicationModal
            isOpen={isAddApplicationModalOpen}
            onModalClose={this.onAddApplicationModalClose}
          />

          <EditApplicationModalConnector
            isOpen={isEditApplicationModalOpen}
            onModalClose={this.onEditApplicationModalClose}
          />
        </PageSectionContent>
      </FieldSet>
    );
  }
}

Applications.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  tagList: PropTypes.arrayOf(PropTypes.object).isRequired,
  onConfirmDeleteApplication: PropTypes.func.isRequired
};

export default Applications;
