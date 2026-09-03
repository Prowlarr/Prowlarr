import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Button from 'Components/Link/Button';
import Menu from 'Components/Menu/Menu';
import MenuContent from 'Components/Menu/MenuContent';
import { sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddIndexerProxyPresetMenuItem from './AddIndexerProxyPresetMenuItem';
import styles from './AddIndexerProxyItem.css';

class AddIndexerProxyItem extends Component {

  //
  // Listeners

  onIndexerProxySelect = () => {
    const {
      implementation,
      implementationName
    } = this.props;

    this.props.onIndexerProxySelect({ implementation, implementationName });
  };

  //
  // Render

  render() {
    const {
      implementation,
      implementationName,
      infoLink,
      presets,
      onIndexerProxySelect
    } = this.props;

    const hasPresets = !!presets && !!presets.length;

    return (
      <Card
        className={styles.indexerProxy}
        overlayClassName={styles.overlay}
        ariaLabel={translate('AddIndexerProxyImplementation', { implementationName })}
        title={implementationName}
        overlayContent={true}
        onPress={this.onIndexerProxySelect}
      >
        <div className={styles.name}>
          {implementationName}
        </div>

        <div className={styles.actions}>
          {
            hasPresets &&
              <span>
                <Button
                  size={sizes.SMALL}
                  onPress={this.onIndexerProxySelect}
                >
                  Custom
                </Button>

                <Menu className={styles.presetsMenu}>
                  <Button
                    className={styles.presetsMenuButton}
                    size={sizes.SMALL}
                  >
                    Presets
                  </Button>

                  <MenuContent>
                    {
                      presets.map((preset) => {
                        return (
                          <AddIndexerProxyPresetMenuItem
                            key={preset.name}
                            name={preset.name}
                            implementation={implementation}
                            implementationName={implementationName}
                            onPress={onIndexerProxySelect}
                          />
                        );
                      })
                    }
                  </MenuContent>
                </Menu>
              </span>
          }

          <Button
            to={infoLink}
            size={sizes.SMALL}
          >
            {translate('MoreInfo')}
          </Button>
        </div>
      </Card>
    );
  }
}

AddIndexerProxyItem.propTypes = {
  implementation: PropTypes.string.isRequired,
  implementationName: PropTypes.string.isRequired,
  infoLink: PropTypes.string.isRequired,
  presets: PropTypes.arrayOf(PropTypes.object),
  onIndexerProxySelect: PropTypes.func.isRequired
};

export default AddIndexerProxyItem;
